using Microsoft.Extensions.Logging;
using Prose.Core.Interfaces;

namespace Prose.Core.Services.Discussion;

/// <summary>Audio ready to play, and what it cost to make.</summary>
/// <param name="Engine">Which voice spoke, for the UI to report honestly.</param>
/// <param name="Free">True when nothing was spent. Drives whether the UI is willing to replay
/// on a whim or has to ask first.</param>
public sealed record SpokenAudio(byte[] Bytes, string MimeType, string Engine, bool Free);

/// <summary>Why read-aloud cannot run, in words the author can act on.</summary>
public sealed record ReadAloudAvailability(
    bool LocalReady,
    bool CloudReady,
    string? LocalBlocker)
{
    public bool Any => LocalReady || CloudReady;
}

/// <summary>
/// Speaking a passage back to the author.
///
/// <para><b>Local first, always.</b> Read-aloud is meant to be used constantly — the same line
/// replayed a dozen times while an ear decides whether it lands. A per-character bill turns that
/// into something you ration, and a voice loop you ration is not a voice loop. Piper is free,
/// offline and unlimited; ElevenLabs is reserved for the deliberate "hear it in the book's own
/// narration voice", which is a different question and worth paying for.</para>
///
/// <para>Nothing here is new synthesis code. <see cref="NodeWorkbenchService.SynthesizeLocalBeatAsync"/>
/// and <see cref="ITtsService.SynthesizeAsync"/> both already return bytes for arbitrary text and
/// persist nothing; until now neither had a single caller in the UI.</para>
/// </summary>
public sealed class ReadAloudService(
    NodeWorkbenchService workbench,
    ITtsService cloud,
    SettingsService settings,
    ILogger<ReadAloudService> log)
{
    /// <summary>The local engine to try. Piper is the one with voices already downloaded.</summary>
    private const string LocalEngine = "piper";

    /// <summary>
    /// What can actually speak right now, and why not.
    ///
    /// <para>Checked rather than assumed because the local engine has three separate ways to be
    /// half-installed: the binary, a voice, and that voice's <c>.json</c> sidecar — which
    /// <c>PiperTtsService</c> requires and which is easy to miss when downloading models.</para>
    /// </summary>
    public ReadAloudAvailability Probe()
    {
        string? blocker = null;
        var localReady = false;

        try
        {
            var engine = LocalTts.Resolve(LocalEngine, log);
            if (engine is null) blocker = $"No local engine named '{LocalEngine}'.";
            else if (!engine.IsAvailable)
                blocker = "Piper is not installed. It needs piper.exe plus a voice — both the "
                        + "*.onnx and its matching *.onnx.json — in %LOCALAPPDATA%\\Prose\\piper, "
                        + "or PROSE_PIPER_EXE and PROSE_PIPER_MODEL pointing at them.";
            else localReady = true;
        }
        catch (Exception ex)
        {
            blocker = ex.Message;
        }

        return new ReadAloudAvailability(
            localReady,
            !string.IsNullOrWhiteSpace(settings.ElevenLabsApiKey),
            blocker);
    }

    /// <summary>
    /// Speak a passage.
    /// </summary>
    /// <param name="bookVoice">Use the book's own narration voice — deliberate, costs characters.
    /// Otherwise the free local voice, falling back to cloud only if there is no local one.</param>
    public async Task<SpokenAudio> SpeakAsync(
        string text, bool bookVoice = false, CancellationToken ct = default)
    {
        // Entity tags, emphasis markers and stage directions are for the page, not the ear.
        var spoken = NarrationText.Clean(text ?? "");
        if (string.IsNullOrWhiteSpace(spoken))
            throw new InvalidOperationException("There is nothing in that selection to read.");

        var availability = Probe();

        if (!bookVoice && availability.LocalReady)
        {
            var wav = await workbench.SynthesizeLocalBeatAsync(spoken, LocalEngine, ct);
            // Null means ffmpeg is missing — Piper emits raw PCM and cannot be played without it.
            if (wav is not null) return new SpokenAudio(wav, "audio/wav", "piper", Free: true);
            log.LogWarning("Piper produced no audio (ffmpeg missing?); falling back to cloud.");
        }

        if (!availability.CloudReady)
            throw new InvalidOperationException(
                availability.LocalBlocker
                ?? "No voice is configured. Add an ElevenLabs key in Settings, or install Piper "
                 + "for a free local voice.");

        var mp3 = await cloud.SynthesizeAsync(spoken, voiceId: null, ct);
        return new SpokenAudio(mp3, "audio/mpeg", "elevenlabs", Free: false);
    }
}
