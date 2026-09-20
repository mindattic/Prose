using Prose.Core.Services.Discussion;

namespace Prose.WriterUi.Services;

/// <summary>
/// Read-aloud for the Writer.
///
/// <para>Scoped like the other UI services. It holds no audio: bytes are synthesized, handed
/// straight to the browser as base64, and forgotten. Nothing is cached and nothing is written to
/// disk — a passage read aloud is a glance, not an artefact, and <c>AuditionVoiceAsync</c>'s own
/// contract is that it persists nothing.</para>
/// </summary>
public sealed class ReadAloudUiService(ReadAloudService readAloud)
{
    /// <param name="Base64">Ready for a <c>data:</c> URL.</param>
    /// <param name="Free">Whether this replay cost anything, so the UI can say so.</param>
    public sealed record Spoken(string Base64, string MimeType, string Engine, bool Free);

    /// <summary>What can speak right now, and what is missing if nothing can.</summary>
    public ReadAloudAvailability Probe() => readAloud.Probe();

    public async Task<Spoken> SpeakAsync(string text, bool bookVoice = false, CancellationToken ct = default)
    {
        var audio = await readAloud.SpeakAsync(text, bookVoice, ct);
        return new Spoken(Convert.ToBase64String(audio.Bytes), audio.MimeType, audio.Engine, audio.Free);
    }
}
