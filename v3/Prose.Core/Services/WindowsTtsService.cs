using System.Runtime.Versioning;
using System.Speech.Synthesis;

namespace Prose.Core.Services;

/// <summary>
/// Free draft narration using Windows built-in speech synthesis (SAPI).
/// Zero cost, zero API keys, instant. Quality is basic but good enough
/// for previewing pacing and flow during writing.
/// </summary>
[SupportedOSPlatform("windows")]
public class WindowsTtsService
{
    public Task<byte[]> SynthesizeAsync(string text, CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            using var synth = new SpeechSynthesizer();
            using var stream = new MemoryStream();

            synth.SetOutputToWaveStream(stream);

            // Pick the best available voice — prefer a male voice for noir
            var voices = synth.GetInstalledVoices()
                .Where(v => v.Enabled)
                .ToList();

            var preferred = voices.FirstOrDefault(v =>
                v.VoiceInfo.Gender == VoiceGender.Male &&
                v.VoiceInfo.Culture.TwoLetterISOLanguageName == "en")
                ?? voices.FirstOrDefault();

            if (preferred != null)
                synth.SelectVoice(preferred.VoiceInfo.Name);

            synth.Rate = 0; // Normal speed
            synth.Speak(text);

            return stream.ToArray();
        }, ct);
    }
}
