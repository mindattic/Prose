using MindAttic.Vault.Launch;
using Prose.Core.Services;
using Prose.Core.Services.Operator;

namespace Prose.WriterUi.Services;

/// <summary>
/// What the Writer shows about Prose's settings and keys.
///
/// <para>LAMP (PRS-T829): Prose's app settings and API keys are edited only in MindAttic Vault
/// (Vault.Desktop, Prose tab). The Writer no longer edits any key: it reports which discussion
/// provider is usable and opens Vault. <see cref="OpenVault"/> runs on the Hub, which is the user's
/// own machine, so it reaches Vault.Desktop's local pipe; <see cref="VaultLink"/> is the
/// <c>mindattic-vault://</c> link for a browser elsewhere.</para>
///
/// <para>Keys are never read back out: only whether one is set and where it comes from.</para>
/// </summary>
public sealed class SettingsUiService(
    OperatorByoKeyPoolService byoKeys,
    SettingsService settings)
{
    public IReadOnlyList<string> Providers => OperatorByoKeyPoolService.Providers;

    /// <summary>Which provider the discussion will actually use: the first with a key, in the
    /// order the tool-calling list is registered (Claude, then OpenAI). Keys assigned to Prose in
    /// MindAttic Vault come first, then the <c>prose --set-byo-key</c> pool.</summary>
    public string? ActiveProvider => Providers.FirstOrDefault(p => KeySource(p) is not null);

    /// <summary>Where the provider's discussion keys come from, or null when it has none. Never a key.</summary>
    public string? KeySource(string provider)
    {
        if (SettingsService.AssignedVaultKeys(provider).Count > 0) return "MindAttic Vault (assigned to Prose)";
        if (byoKeys.GetPool(provider).Count > 0) return "prose --set-byo-key pool";
        return null;
    }

    /// <summary>Whether an ElevenLabs key resolves (Vault, PROSE_ELEVENLABS_API_KEY, or legacy settings).</summary>
    public bool HasElevenLabsKey => !string.IsNullOrWhiteSpace(settings.ElevenLabsApiKey);

    /// <summary>
    /// Opens MindAttic.Vault.Desktop at Prose's tab (optionally scrolled to one setting's full name).
    /// False when Vault.Desktop isn't installed or couldn't be reached.
    /// </summary>
    public bool OpenVault(string? setting = null) => VaultSettings.Open(ProseSettings.AppId, setting);

    /// <summary>The <c>mindattic-vault://</c> link to Prose's tab, for a browser on another machine.</summary>
    public string VaultLink => VaultSettings.Link(ProseSettings.AppId);
}
