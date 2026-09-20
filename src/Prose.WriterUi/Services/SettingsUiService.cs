using Prose.Core.Services;
using Prose.Core.Services.Operator;

namespace Prose.WriterUi.Services;

/// <summary>
/// The credential settings the Writer can change.
///
/// <para><b>This is the first settings surface in any Prose app.</b> Until now the only ways to
/// set a key were <c>prose --set-byo-key</c> and the MCP tools — while <c>narrate_node</c> and the
/// audiobook exporter have been failing with "Set it in Settings" pointing at a screen that did
/// not exist.</para>
///
/// <para>Keys are never read back out. <see cref="Mask"/> is all the UI ever sees, so a key cannot
/// be lifted off the screen, out of a screenshot, or out of the Blazor circuit's payload.</para>
/// </summary>
public sealed class SettingsUiService(
    OperatorByoKeyPoolService byoKeys,
    SettingsService settings)
{
    /// <param name="Masked">Last four characters only — enough to tell two keys apart.</param>
    public sealed record KeyRow(string Provider, int Index, string Masked);

    public IReadOnlyList<string> Providers => OperatorByoKeyPoolService.Providers;

    /// <summary>Which provider the discussion will actually use: the first with a key, in the
    /// order the tool-calling list is registered (Claude, then OpenAI).</summary>
    public string? ActiveProvider
        => Providers.FirstOrDefault(p => byoKeys.GetPool(p).Count > 0);

    public IReadOnlyList<KeyRow> KeysFor(string provider)
        => byoKeys.GetPool(provider)
                  .Select((k, i) => new KeyRow(provider, i, Mask(k)))
                  .ToList();

    public void AddKey(string provider, string key)
    {
        key = key.Trim();
        if (key.Length == 0) return;
        byoKeys.AddKey(provider, key);
    }

    /// <summary>Remove by position, because the UI never holds the key itself.</summary>
    public void RemoveAt(string provider, int index)
    {
        var pool = byoKeys.GetPool(provider).ToList();
        if (index < 0 || index >= pool.Count) return;
        pool.RemoveAt(index);
        byoKeys.SetPool(provider, pool);
    }

    /// <summary>ElevenLabs sits in a different store entirely — the <see cref="SettingsService"/>
    /// vault chain, not the BYO pool — so it is read and written separately rather than pretending
    /// the two are one system.</summary>
    public bool HasElevenLabsKey => !string.IsNullOrWhiteSpace(settings.ElevenLabsApiKey);

    public void SetElevenLabsKey(string key) => settings.ElevenLabsApiKey = key.Trim();

    private static string Mask(string key)
        => key.Length >= 4 ? "…" + key[^4..] : "(short key)";
}
